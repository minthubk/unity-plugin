//
//  AppingtonManager.h
//  Unity-iPhone
//
//  Created by Mike Desaro on 4/16/13.
//
//

#import <Foundation/Foundation.h>


@interface AppingtonManager : NSObject


+ (AppingtonManager*)sharedManager;

+ (id)objectFromJson:(NSString*)json;



- (void)control:(NSString*)name andValues:(NSDictionary*)values;


@end
